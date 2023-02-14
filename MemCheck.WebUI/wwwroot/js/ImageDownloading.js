import { imageSizeMedium } from './Common.js';
import { base64FromBytes } from './Common.js';
import { getMnesiosImageNamesFromSourceText } from './MarkdownConversion.js';

async function downloadImageBlobAsNecessary(image) { // image has a field 'name'. This method will download the blob and add it to the image in a field named blob
    if (!image.name)
        throw new Error('image object does not feature a name field');
    if (image.blob)
        return;

    await axios.post('/Learn/GetImageByName/', { imageName: image.name, size: imageSizeMedium }, { responseType: 'arraybuffer' })
        .then(result => {
            image.blob = base64FromBytes(result.data);
        })
        .catch(() => {
            // Just ignore, and the blob won't be available
        });
}

async function downloadImageDetailsAsNecessary(image) { // image has a field 'name'. This method will download the details and add them to the image in fields
    if (!image.name)
        throw new Error('image object does not feature a name field');
    if (image.description) // Assume this means we have all the details
        return;

    await axios.post('/Media/GetImageMetadataFromName/', { imageName: image.name })
        .then(result => {
            image.imageId = result.data.imageId;
            image.description = result.data.description;
            image.source = result.data.source;
            image.initialUploadUtcDate = result.data.initialUploadUtcDate;
            image.initialVersionCreator = result.data.initialVersionCreator;
            image.currentVersionUtcDate = result.data.currentVersionUtcDate;
            image.currentVersionDescription = result.data.currentVersionDescription;
            image.cardCount = result.data.cardCount;
            image.originalImageContentType = result.data.originalImageContentType;
            image.originalImageSize = result.data.originalImageSize;
            image.smallSize = result.data.smallSize;
            image.mediumSize = result.data.mediumSize;
            image.bigSize = result.data.bigSize;
        })
        .catch(() => {
            // Just ignore, and the details won't be available
        });
}

async function loadImageBlobAndDetailsAsNecessary(image) {
    const getImageByNamePromise = downloadImageBlobAsNecessary(image);
    const getImageMetadataFromNamePromise = downloadImageDetailsAsNecessary(image);
    await Promise.all([getImageByNamePromise, getImageMetadataFromNamePromise]);
}

export async function downloadMissingImages(text, imageList) {
    const neededImageNames = getMnesiosImageNamesFromSourceText(text);
    const newImageNames = [...neededImageNames].filter(imageName => !imageList.find(imageDefinition => imageDefinition.name === imageName));
    newImageNames.forEach(newImageName => imageList.push({ name: newImageName }));

    const loadsToWait = [];
    imageList.forEach(img => { const loadPromise = loadImageBlobAndDetailsAsNecessary(img); loadsToWait.push(loadPromise); });
    await Promise.all(loadsToWait);
}
